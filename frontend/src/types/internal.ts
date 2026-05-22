import type { RawLesson } from './modules';

export interface TimeTableDayInfo {
	lessonSchedule: RawLesson;
	moduleCode: string;
	moduleName: string;
	normalisedStartDuration: number;
	normalisedEndDuration: number;
	isAChoiceSelection: boolean;
	uniqueIdentifer: string;
	hasFoundAGroup: boolean;
	groupLength: number;
	groupIndex: number;
}
