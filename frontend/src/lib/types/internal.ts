import type { Profile } from './db_raw_types';
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
	timetableColour: string;
	timetableId: string;
	timetableOwner: Profile | undefined;
}
