import type { TimetableDetailedResponse } from "$lib/types/db_raw_types";
import type { Module, RawLesson } from "$lib/types/modules";

export interface FullLessonInformation extends RawLesson {
  module: Module;
}

export interface TimetableFullInformation extends TimetableDetailedResponse {
  rawLesson: FullLessonInformation[]
}
