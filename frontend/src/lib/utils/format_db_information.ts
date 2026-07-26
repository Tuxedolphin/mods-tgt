import type { LessonInfo } from "$lib/shared/shared.svelte";
import type {
  TimetableDetailedResponse,
  TimetableModule,
} from "$lib/types/db_raw_types";
import type { TimeTableDayInfo } from "$lib/types/internal";
import type { RawLesson } from "$lib/types/modules";
import { orderBy, remove } from "es-toolkit";

import { normaliseDuration } from "./calculations_for_ui";
import { getFullModInfo } from "./fetch_from_cache";
import { default_colour_fallback } from "./formatting_utils";

const daysOfWeek = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
const startOfDayTime = "0800";
const endOfDayTime = "2000";
export function getTimetable(
  acadYear: string,
  semesterNo: number,
  timetables: TimetableDetailedResponse[],
): TimetableDetailedResponse[] {
  const timetable = timetables.filter(
    (x) => x.academicYear == acadYear && x.semester == semesterNo,
  );

  return timetable.length === 0 ? [] : timetable;
}

export async function queryAvailableLessons(
  day: number,
  semester: number,
  acadYear: string,
  userState: LessonInfo,
): Promise<TimeTableDayInfo[]> {
  const resultingTimetables: TimeTableDayInfo[] = [];

  if (userState.moduleCode === "") return resultingTimetables;
  const modInfo = await getFullModInfo(userState.moduleCode, acadYear);
  const weekData = modInfo?.semesterData.find(
    (semNo) => semNo.semester == semester,
  );
  const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);
  const lessonTypeToMatch = ttData?.filter(
    (x) => x.lessonType == userState.lessonType,
  );
  if (lessonTypeToMatch) {
    for (const lesson of lessonTypeToMatch!) {
      if (lesson.classNo == userState.classNo) continue;
      let lesson_info: TimeTableDayInfo = {
        lessonSchedule: lesson,
        moduleCode: userState.moduleCode,
        moduleName: modInfo.title,
        normalisedStartDuration: normaliseDuration(
          startOfDayTime,
          endOfDayTime,
          lesson.startTime,
        ),
        normalisedEndDuration: normaliseDuration(
          startOfDayTime,
          endOfDayTime,
          lesson.endTime,
        ),
        lessonLength: 0,
        isAChoiceSelection: true,
        innerGroupIndex: -1,
        innerGroupLength: -1,
        timetableColour: userState.colour,
        timetableId: userState.selectedTimetableId,
        timetableOwner: undefined,
        processed: false,
      };

      lesson_info.lessonLength =
        lesson_info.normalisedEndDuration - lesson_info.normalisedStartDuration;
      resultingTimetables.push(lesson_info);
    }
  }

  return resultingTimetables;
}

export async function filterTimetableByDay(
  day: number,
  timetables: TimetableDetailedResponse[],
): Promise<TimeTableDayInfo[]> {
  if (timetables.length === 0) return [];
  const resultingTimetables: TimeTableDayInfo[] = [];

  for (const timetable of timetables) {
    for (const lesson of timetable.metaData) {
      const modInfo = await getFullModInfo(
        lesson.moduleCode,
        timetable.academicYear,
      );

      const weekData = modInfo.semesterData.find(
        (x) => x.semester == timetable.semester,
      )!;
      const lessonForDay = weekData.timetable.filter(
        (x) =>
          x.day == daysOfWeek[day] &&
          x.lessonType == lesson.lessonType &&
          x.classNo == lesson.lessonNo,
      );

      for (const lessonDayInfo of lessonForDay) {
        let lesson_info = {
          innerGroupIndex: -1,
          innerGroupLength: -1,
          isAChoiceSelection: false,
          lessonSchedule: lessonDayInfo,
          normalisedEndDuration: normaliseDuration(
            startOfDayTime,
            endOfDayTime,
            lessonDayInfo.endTime,
          ),
          lessonLength: 0,
          normalisedStartDuration: normaliseDuration(
            startOfDayTime,
            endOfDayTime,
            lessonDayInfo.startTime,
          ),
          moduleCode: lesson.moduleCode,
          moduleName: modInfo.title,
          timetableColour: timetable.profile.colour
            ? timetable.profile.colour
            : default_colour_fallback,
          timetableId: timetable.id,
          timetableOwner: timetable.profile,
          processed: false,
        };

        lesson_info.lessonLength =
          lesson_info.normalisedEndDuration -
          lesson_info.normalisedStartDuration;
        resultingTimetables.push(lesson_info);
      }
    }
  }
  return resultingTimetables;
}

export function removeModEntry(
  timetable: TimetableDetailedResponse[],
  acadYear: string,
  semesterNo: number,
  id: string,
  moduleCode: string,
): TimetableDetailedResponse[] {
  const findTimetableCopy = timetable.filter(
    (x) => x.id == id && x.academicYear == acadYear && x.semester == semesterNo,
  )[0];
  for (let index = findTimetableCopy.metaData.length - 1; index >= 0; index--) {
    const element = findTimetableCopy.metaData[index];
    if (element.moduleCode == moduleCode) {
      findTimetableCopy.metaData.splice(index, 1);
    }
  }

  return timetable;
}

export function modifyModColour(
  timetable: TimetableDetailedResponse[],
  acadYear: string,
  semesterNo: number,
  id: string,
  moduleCode: string,
  newColor: string,
): TimetableDetailedResponse[] {
  const findTimetableCopy = timetable.filter(
    (x) => x.id == id && x.academicYear == acadYear && x.semester == semesterNo,
  )[0];

  const lessonRef = findTimetableCopy.metaData.filter(
    (x) => x.moduleCode == moduleCode,
  )!;

  for (let i = 0; i < lessonRef.length; i++) {
    const element = lessonRef[i];
    element.colour = newColor;
  }

  return timetable;
}
export function modifyModEntry(
  timetable: TimetableDetailedResponse[],
  acadYear: string,
  semesterNo: number,
  id: string,
  moduleCode: string,
  lessonType: string,
  newlessonNo: string,
  userState: LessonInfo,
): TimetableDetailedResponse[] {
  if (
    moduleCode != userState.moduleCode ||
    lessonType != userState.lessonType
  ) {
    return timetable;
  }
  const findTimetableCopy = timetable.filter(
    (x) => x.id == id && x.academicYear == acadYear && x.semester == semesterNo,
  )[0];
  const lessonRef = findTimetableCopy.metaData.find(
    (x) =>
      x.lessonType == userState.lessonType &&
      x.moduleCode == userState.moduleCode &&
      x.lessonNo == userState.classNo,
  )!;

  lessonRef.lessonNo = newlessonNo;

  return timetable;
}

export function checkModAlreadyAdded(
  timetable: TimetableDetailedResponse[],
  acadYear: string,
  semesterNo: number,
  id: string | null,
  moduleCode: string,
): boolean {
  if (id === null) return false;
  const findTimetableCopy = timetable.filter(
    (x) => x.id == id && x.academicYear == acadYear && x.semester == semesterNo,
  );

  if (findTimetableCopy.length == 0) return false;

  return (
    findTimetableCopy[0].metaData.findIndex(
      (x) => x.moduleCode == moduleCode,
    ) !== -1
  );
}

export async function createModEntry(
  timetable: TimetableDetailedResponse[],
  acadYear: string,
  semesterNo: number,
  id: string,
  moduleCode: string,
  rawLesson: RawLesson[],
  user_colour: string,
): Promise<TimetableDetailedResponse[]> {
  const findTimetableCopy = timetable.filter(
    (x) => x.id == id && x.academicYear == acadYear && x.semester == semesterNo,
  );
  const lessonDataRef: TimetableModule[] = [];

  const lessonTypes = Object.groupBy(rawLesson, (x) => x.lessonType);
  const assigned_color = user_colour;
  for (const lessonType in lessonTypes) {
    const lesson = lessonTypes[lessonType]![0];
    lessonDataRef.push({
      lessonNo: lesson.classNo,
      lessonType: lesson.lessonType,
      moduleCode: moduleCode,
      colour: assigned_color,
    });
  }

  if (findTimetableCopy.length == 0) {
    // timetable[0].LessonData = lessonDataRef;
  } else {
    findTimetableCopy[0].metaData.push(...lessonDataRef);
  }

  return timetable;
}

export function findOverlappingTimeInfo(
  allTime: TimeTableDayInfo[],
  users_to_hide: string[],
): TimeTableDayInfo[] {
  allTime = orderBy(
    allTime,
    ["normalisedStartDuration", "lessonLength"],
    ["asc", "desc"],
  );
  remove(allTime, (x) =>
    users_to_hide.includes(
      x.timetableOwner?.userId ? x.timetableOwner!.userId : "",
    ),
  );

  for (let i = 0; i < allTime.length; i++) {
    const element = allTime[i];
    element.processed = false;
    element.innerGroupLength = -1;
    element.innerGroupIndex = -1;
  }

  const MAX_ITER = 1000;
  let iter_count = 0;
  let not_all_processed = true;
  while (not_all_processed && MAX_ITER != iter_count) {
    // find first lesson to compare:
    let time_to_compare: TimeTableDayInfo | undefined = undefined;
    let groups = [];

    for (let i = 0; i < allTime.length; i++) {
      const element = allTime[i];
      if (!element.processed) {
        time_to_compare = element;
        time_to_compare.processed = true;
        groups.push(time_to_compare);
        break;
      }
    }

    // Find similar timings:
    let first_hit = true;
    for (let i = 0; i < allTime.length; i++) {
      const compared_timing = allTime[i];

      if (compared_timing.processed) continue;

      if (
        compared_timing.normalisedStartDuration >=
          time_to_compare!.normalisedStartDuration &&
        compared_timing.normalisedEndDuration <=
          time_to_compare!.normalisedEndDuration
      ) {
        compared_timing.processed = true;
        if (first_hit) {
          time_to_compare!.processed = false;
          first_hit = false;
          time_to_compare = compared_timing;
        }

        groups.push(compared_timing);
      }
    }
    iter_count++;
    for (let i = 0; i < groups.length; i++) {
      const element = groups[i];
      element.innerGroupIndex = i;
      if (element.innerGroupLength === -1) {
        element.innerGroupLength = groups.length;
      }
    }

    not_all_processed = false;
    for (let i = 0; i < allTime.length; i++) {
      const element = allTime[i];
      if (!element.processed) {
        not_all_processed = true;
        break;
      }
    }
  }

  if (iter_count === MAX_ITER) {
    console.error("Unable to find pairings");
  }
  return allTime;
}
