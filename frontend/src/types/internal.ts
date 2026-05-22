import type { RawLesson } from './modules';

export interface TimeTableDayInfo {
	lessonSchedule: RawLesson;
	moduleCode: string;
	moduleName: string;
	normalisedStartDuration: number;
	normalisedEndDuration: number;
	searchedModuleCodes: Set<string>;
	isAChoiceSelection: boolean;
	uniqueIdentifer: string;
}
