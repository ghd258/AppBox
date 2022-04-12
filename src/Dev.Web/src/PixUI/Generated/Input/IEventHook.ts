import * as System from '@/System'
import * as PixUI from '@/PixUI'

export interface IEventHook {
    PreviewEvent(type: PixUI.EventType, e: Nullable<object>): EventPreviewResult;
}

export enum EventPreviewResult {
    NotProcessed = 0,

    Processed = 1,

    NoDispatch = EventPreviewResult.Processed << 1,

    NoContinue = EventPreviewResult.NoDispatch << 1,

    ProcessedNoDispatch = EventPreviewResult.Processed | EventPreviewResult.NoDispatch,

    All = EventPreviewResult.Processed | EventPreviewResult.NoDispatch | EventPreviewResult.NoContinue
}
