import type { RawLesson } from "./modules";

	export interface TimeTableDayInfo {
		lessonSchedule: RawLesson;
		moduleCode: string;
		moduleName: string;
	}
