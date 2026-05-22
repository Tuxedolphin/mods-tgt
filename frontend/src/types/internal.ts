import type { RawLesson } from './modules';

export interface TimeTableDayInfo {
	lessonSchedule: RawLesson;
	moduleCode: string;
	moduleName: string;
	normalisedStartDuration: number;
	normalisedEndDuration: number;
	indexInRow: number;
	maxNumGroup: number;
	searchedModuleCodes: Set<string>;
}
