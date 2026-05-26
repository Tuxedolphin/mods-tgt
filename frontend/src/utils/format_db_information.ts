import type { TimeTableDayInfo } from '../types/internal';
import type { TimeTable } from '../types/mod_summaries';
import { normaliseDuration } from './calculations_for_ui';
import { getFullModInfo } from './fetch_from_cache';

const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
const startOfDayTime = '0800';
const endOfDayTime = '2000';
export function getTimetable(
	acadYear: string,
	semesterNo: number,
	timetables: TimeTable[]
): TimeTable[] {
	console.log(timetables);
	const timetable = timetables.filter(
		(x) => x.AcademicYear == acadYear && x.Semester == semesterNo
	);

	return timetable.length === 0 ? [] : timetable;
}

export async function filterTimetableByDay(
	day: number,
	timetables: TimeTable[]
): Promise<TimeTableDayInfo[]> {
	if (timetables.length === 0) return [];
	const resultingTimetables: TimeTableDayInfo[] = [];

	for (const timetable of timetables) {
		for (const lesson of timetable.LessonData) {
			const modInfo = await getFullModInfo(lesson.ModuleCode, timetable.AcademicYear);

			const weekData = modInfo.semesterData.find((x) => x.semester == timetable.Semester)!;
			const lessonForDay = weekData.timetable.filter(
				(x) =>
					x.day == daysOfWeek[day] &&
					x.lessonType == lesson.LessonType &&
					x.classNo == lesson.LessonType
			);

			for (const lessonDayInfo of lessonForDay) {
				resultingTimetables.push({
					innerGroupIndex: -1,
					innerGroupLength: -1,
					outerGroupIndex: -1,
					outerGroupLength: -1,
					isAChoiceSelection: false,
					lessonSchedule: lessonDayInfo,
					normalisedEndDuration: normaliseDuration(
						startOfDayTime,
						endOfDayTime,
						lessonDayInfo.endTime
					),
					normalisedStartDuration: normaliseDuration(
						startOfDayTime,
						endOfDayTime,
						lessonDayInfo.startTime
					),
					moduleCode: lesson.ModuleCode,
					moduleName: modInfo.title
				});
			}
		}
	}
	return resultingTimetables;
}
