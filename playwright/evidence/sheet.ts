import type { Browser, Page } from '@playwright/test';
import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
export const staging = resolve(root, 'playwright/test-results-evidence/staging');
mkdirSync(staging, { recursive: true });

const at = (): string => new Date().toISOString();
const esc = (s: string): string => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

/** A full-page PNG of the real app, plus an ARIA snapshot sidecar so the content can be checked as text. */
export async function shot(page: Page, name: string): Promise<string> {
  const path = resolve(staging, `${name}.png`);
  await page.screenshot({ path, fullPage: true });
  writeFileSync(resolve(staging, `${name}.aria.yml`), `# ${name} · ${page.url()} · ${at()}\n${await page.locator('body').ariaSnapshot()}\n`);
  return path;
}

/** Several real screenshots on one sheet, each with its caption. */
export async function sheet(browser: Browser, panels: { png: string; caption: string }[], name: string, cols: number, title: string): Promise<void> {
  const width = 1280;
  const page = await browser.newPage({ viewport: { width: cols * (width / 2) + (cols + 1) * 16, height: 600 }, deviceScaleFactor: 2 });
  const figs = panels
    .map((p) => `<figure style="margin:0"><figcaption style="margin:0 0 6px">${esc(p.caption)}</figcaption>
      <img style="width:${width / 2}px;border:1px solid #ccc" src="data:image/png;base64,${readFileSync(p.png).toString('base64')}"></figure>`)
    .join('');
  await page.setContent(`<!doctype html><body style="margin:0;padding:16px;background:#fff;font:600 14px system-ui;color:#111">
    <h1 style="font-size:18px;margin:0 0 4px">${esc(title)}</h1>
    <p style="margin:0 0 12px;font-weight:400;color:#444">WeighingMS on the isolated E2E stack (Compose full) · captured ${at()}</p>
    <div style="display:grid;grid-template-columns:repeat(${cols},${width / 2}px);gap:16px">${figs}</div></body>`);
  await page.screenshot({ path: resolve(staging, `${name}.png`), fullPage: true });
  await page.close();
}

/** Real output (JSON or text) on a terminal-style page, captioned with where it came from. */
export async function render(browser: Browser, o: { title: string; source: string; text: string; note?: string; columns?: number }, name: string): Promise<void> {
  const page = await browser.newPage({ viewport: { width: o.columns ? 1920 : 1280, height: 400 } });
  await page.setContent(`<!doctype html><body style="margin:0;padding:20px;background:#0f1419;color:#d7dde4;font:13px/1.45 ui-monospace,Menlo,monospace">
    <div style="font:600 16px system-ui;color:#fff;margin-bottom:4px">${esc(o.title)}</div>
    <div style="font:12px system-ui;color:#9aa5b1;margin-bottom:${o.note ? 4 : 14}px">Source: ${esc(o.source)} · captured ${at()}</div>
    ${o.note ? `<div style="font:12px system-ui;color:#e3b341;margin-bottom:14px">${esc(o.note)}</div>` : ''}
    <pre style="margin:0;padding:14px;background:#161b22;border:1px solid #30363d;border-radius:6px;white-space:pre-wrap;word-break:break-word;${o.columns ? `column-count:${o.columns};column-gap:28px;font-size:11px` : ''}">${esc(o.text)}</pre></body>`);
  await page.screenshot({ path: resolve(staging, `${name}.png`), fullPage: true });
  writeFileSync(resolve(staging, `${name}.source.txt`), `${o.title}\n${o.source}\n\n${o.text}\n`);
  await page.close();
}

/** Copies the test's recorded video into staging once the page is closed. */
export async function keepVideo(page: Page, name: string): Promise<void> {
  const video = page.video();
  await page.close();
  if (!video) throw new Error('video recording is off');
  copyFileSync(await video.path(), resolve(staging, `${name}.webm`));
}

export const json = (v: unknown): string => JSON.stringify(v, null, 2);
