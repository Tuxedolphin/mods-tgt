import type { LessonInfo } from '../shared/shared.svelte';
import type { TimeTableDayInfo } from '../types/internal';
import type { LessonData, TimeTable } from '../types/mod_summaries';
import type { RawLesson } from '../types/modules';
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
	const timetable = timetables.filter(
		(x) => x.AcademicYear == acadYear && x.Semester == semesterNo
	);

	return timetable.length === 0 ? [] : timetable;
}

export async function queryAvailableLessons(
	day: number,
	semester: number,
	acadYear: string,
	userState: LessonInfo
): Promise<TimeTableDayInfo[]> {
	const resultingTimetables: TimeTableDayInfo[] = [];

	if (userState.moduleCode === '') return resultingTimetables;

	const modInfo = await getFullModInfo(userState.moduleCode, acadYear);
	const weekData = modInfo?.semesterData.find((semNo) => semNo.semester == semester);
	const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);
	const lessonTypeToMatch = ttData?.filter((x) => x.lessonType == userState.lessonType);
	if (lessonTypeToMatch) {
		for (const lesson of lessonTypeToMatch!) {
			if (lesson.classNo == userState.classNo) continue;
			resultingTimetables.push({
				lessonSchedule: lesson,
				moduleCode: userState.moduleCode,
				moduleName: modInfo.title,
				normalisedStartDuration: normaliseDuration(startOfDayTime, endOfDayTime, lesson.startTime),
				normalisedEndDuration: normaliseDuration(startOfDayTime, endOfDayTime, lesson.endTime),
				isAChoiceSelection: true,
				innerGroupIndex: -1,
				innerGroupLength: -1,
				outerGroupIndex: -1,
				outerGroupLength: -1
			});
		}
	}

	return resultingTimetables;
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
					x.classNo == lesson.LessonNo
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

export async function modifyModEntry(
	timetable: TimeTable[],
	acadYear: string,
	semesterNo: number,
	owner: string,
	timetableName: string,
	moduleCode: string,
	lessonType: string,
	newLessonNo: string,
	userState: LessonInfo
) {
	if (moduleCode != userState.moduleCode || lessonType != userState.lessonType) {
		return timetable;
	}
	const findTimetableCopy = timetable.filter(
		(x) =>
			x.Owner == owner &&
			x.AcademicYear == acadYear &&
			x.Semester == semesterNo &&
			x.Name == timetableName
	)[0];
	const lessonRef = findTimetableCopy.LessonData.find(
		(x) =>
			x.LessonType == userState.lessonType &&
			x.ModuleCode == userState.moduleCode &&
			x.LessonNo == userState.classNo
	)!;

	lessonRef.LessonNo = newLessonNo;
	return timetable;
}

export function checkModAlreadyAdded(
	timetable: TimeTable[],
	acadYear: string,
	semesterNo: number,
	owner: string,
	timetableName: string,
	moduleCode: string
): boolean {
	const findTimetableCopy = timetable.filter(
		(x) =>
			x.Owner == owner &&
			x.AcademicYear == acadYear &&
			x.Semester == semesterNo &&
			x.Name == timetableName
	);

	if (findTimetableCopy.length == 0) return false;

	return findTimetableCopy[0].LessonData.findIndex((x) => x.ModuleCode == moduleCode) !== -1;
}

export async function createModEntry(
	timetable: TimeTable[],
	acadYear: string,
	semesterNo: number,
	owner: string,
	timetableName: string,
	moduleCode: string,
	rawLesson: RawLesson[]
): Promise<TimeTable[]> {
	const findTimetableCopy = timetable.filter(
		(x) =>
			x.Owner == owner &&
			x.AcademicYear == acadYear &&
			x.Semester == semesterNo &&
			x.Name == timetableName
	);
	const lessonDataRef: LessonData[] = [];
	// No entries found: create new one:
	if (findTimetableCopy.length == 0) {
		timetable.push({
			AcademicYear: acadYear,
			LessonData: lessonDataRef,
			Name: timetableName,
			Owner: owner,
			Semester: semesterNo
		});
	}
	const lessonTypes = Object.groupBy(rawLesson, (x) => x.lessonType);
	for (const lessonType in lessonTypes) {
		const lesson = lessonTypes[lessonType]![0];
		lessonDataRef.push({
			LessonNo: lesson.classNo,
			LessonType: lesson.lessonType,
			ModuleCode: moduleCode
		});
	}

	if (findTimetableCopy.length == 0) {
		// timetable[0].LessonData = lessonDataRef;
	} else {
		findTimetableCopy[0].LessonData.push(...lessonDataRef);
	}

	return timetable;
}

export function findOverlappingTimeInfo(allTime: TimeTableDayInfo[]): TimeTableDayInfo[] {
	allTime.sort((a, b) => a.normalisedStartDuration - b.normalisedStartDuration);

	const groupTimes = Object.groupBy(allTime, (x) => x.normalisedStartDuration);
	const MAX_ITER = 10_000;
	let iterIdx = 0;
	const processedTimings: string[] = [];
	const processedGroups: {
		[key: number]: TimeTableDayInfo[][];
	} = {};
	let lengthOfGroups = 0;
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	for (const _len in groupTimes) lengthOfGroups++;
	let groupId = 0;
	while (processedTimings.length != lengthOfGroups && iterIdx != MAX_ITER) {
		iterIdx++;
		let firstGroup: TimeTableDayInfo[] = [];
		let firstGroupProcess: string = '';
		for (const i in groupTimes) {
			if (processedTimings.includes(i)) continue;
			firstGroup = groupTimes[i]!;
			firstGroupProcess = i;
			firstGroup.sort(
				(a, b) =>
					b.normalisedEndDuration -
					b.normalisedStartDuration -
					(a.normalisedEndDuration - a.normalisedStartDuration)
			);

			processedTimings.push(i);

			if (!processedGroups[groupId]) processedGroups[groupId] = [];
			processedGroups[groupId].push(firstGroup);
			break;
		}
		if (firstGroup.length === 0) break;

		let endTime = firstGroup[0].normalisedEndDuration;

		// Find groups:
		for (const i in groupTimes) {
			if (i == firstGroupProcess) continue;
			const group = groupTimes[i]![0];
			if (group.normalisedStartDuration >= endTime) {
				endTime = group.normalisedEndDuration;
				processedTimings.push(i);
				processedGroups[groupId].push(groupTimes[i]!);
			}
		}
		groupId++;
	}

	const outerGroupLength = Object.keys(processedGroups).length;

	for (const group in processedGroups) {
		const outerGroupIdx = Number.parseInt(group);

		for (const lessonGrouping of processedGroups[group]) {
			const innerGroupLength = lessonGrouping.length;
			for (let i = 0; i < lessonGrouping.length; i++) {
				const lesson = lessonGrouping[i];
				lesson.outerGroupIndex = outerGroupIdx;
				lesson.outerGroupLength = outerGroupLength;
				lesson.innerGroupLength = innerGroupLength;
				lesson.innerGroupIndex = i;
			}
		}
	}

	if (iterIdx == MAX_ITER) {
		console.log('Unable to find pairings');
	}

	return allTime;
}
