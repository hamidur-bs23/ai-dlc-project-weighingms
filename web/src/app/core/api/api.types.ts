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

export type WeighingSession = Schemas['WeighingSessionResponse'];
export type WeighingComponent = Schemas['WeighingComponentResponse'];
export type WeighingReading = Schemas['WeighingReadingResponse'];
export type WeighingRecord = Schemas['WeighingRecordResponse'];
export type ConfirmationResult = Schemas['ConfirmationResponse'];
export type BatchSummary = Schemas['BatchSummaryResponse'];
export type StepUpGrant = Schemas['StepUpResponse'];

/** The four reading classes the server computes (FR-12). */
export type WeightClass = 'underweight' | 'withinRange' | 'targetAchieved' | 'overweight';

/** The WeighingMS queue status. */
export type BatchStatus = 'queued' | 'weighing' | 'complete' | 'syncPending' | 'syncFailed' | 'printPending';

/** The OrgaTEX write-back of a batch (bolt 011). */
export type OrgatexState = Schemas['OrgatexStateResponse'];
