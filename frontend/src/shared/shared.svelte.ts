import { persisted } from 'svelte-persisted-store'

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