import { persisted } from 'svelte-persisted-store';
import type { SavedModInfo } from '../types/mod_summaries';
import { writable } from 'svelte/store';
// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted(
	'selectedMods',
	{} as {
		[acadYear: string]: {
			[semesterNumber: number]: {
				[moduleCode: string]: SavedModInfo;
			};
		};
	}
);

export const preferences = persisted('prefs', {
	currentSemView: 1,
	acadYear: '2025-2026'
});

export const chooseModState = $state({
	moduleCode: '',
	lessonType: '',
	classNo: ''
}) as LessonInfo;

export const searchTerm = writable('');

export interface LessonInfo {
	moduleCode: string;
	lessonType: string;
	classNo: string;
}
