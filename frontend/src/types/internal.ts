import type { RawLesson } from './modules';
export interface TimeTableDayInfo {
	lessonSchedule: RawLesson;
	moduleCode: string;
	moduleName: string;
	normalisedStartDuration: number;
	normalisedEndDuration: number;
	isAChoiceSelection: boolean;
	outerGroupIndex: number;
	outerGroupLength: number;
	innerGroupIndex: number;
	innerGroupLength: number;
}
