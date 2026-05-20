import { persisted } from 'svelte-persisted-store'
import type { ModSummary } from '../types/mod_summaries';

type LessonInfo = {
  classNo: string,
  lessonType: string
}

type SavedModInfo = {
  [key: string] : LessonInfo
}

// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted('selectedMods', {
  selectedMods: {} as SavedModInfo,
});