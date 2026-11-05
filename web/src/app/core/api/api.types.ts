// The public surface of the generated Api types (openapi-typescript, `npm run api:types`).
// Components and services import these names, never the generated file directly.
import type { components } from './generated/schema';

type Schemas = components['schemas'];

export type Quantity = Schemas['QuantityResponse'];
export type Session = Schemas['SessionResponse'];
export type SignInRequest = Schemas['SignInRequest'];
export type Station = Schemas['StationResponse'];
export type Batch = Schemas['BatchResponse'];
export type BatchList = Schemas['BatchListResponse'];
export type Recipe = Schemas['RecipeResponse'];
export type RecipeComponent = Schemas['RecipeComponentResponse'];
export type DeviceHealth = Schemas['DeviceHealthResponse'];
export type DeviceHealthList = Schemas['DeviceHealthListResponse'];
export type Scale = Schemas['ScaleResponse'];
export type ScaleList = Schemas['ScaleListResponse'];
export type ScanList = Schemas['ScanListResponse'];

/** The WeighingMS queue status (bolts 009/011/012 add complete, print and sync pending). */
export type BatchStatus = 'queued' | 'weighing';
