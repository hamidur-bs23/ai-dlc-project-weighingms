// Dev-server proxy: the SPA and WeighingMS.Api share one origin, so cookie auth and the
// XSRF-TOKEN cookie work without CORS (tech-stack). WMS_API_URL overrides the target, for
// example when an evidence run uses spare ports.
export default {
  '/api': {
    target: process.env['WMS_API_URL'] ?? 'http://localhost:5080',
    secure: false,
    changeOrigin: false,
  },
};
