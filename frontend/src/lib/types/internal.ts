import type { LucideIcon } from "@lucide/svelte";
import type { Profile } from "./db_raw_types";
import type { RawLesson } from "./modules";
export interface TimeTableDayInfo {
  lessonSchedule: RawLesson;
  moduleCode: string;
  moduleName: string;
  normalisedStartDuration: number;
  normalisedEndDuration: number;
  lessonLength: number;
  isAChoiceSelection: boolean;
  innerGroupIndex: number;
  innerGroupLength: number;
  timetableColour: string;
  timetableId: string;
  timetableOwner: Profile | undefined;
  processed: boolean;
}

export type NavigationItemProp = {
  icon: LucideIcon;
  title: string;
  path: string;
};
