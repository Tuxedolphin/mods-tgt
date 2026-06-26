import type { TimetableResponse, Profile } from '$lib/types/db_raw_types';
import { persisted } from 'svelte-persisted-store';
import { writable } from 'svelte/store';
// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted('selectedMods', [] as TimetableResponse[]);

export const registered = writable(false);
export const timetable_list_should_be_refreshed = writable(false);

interface AccessTokenInfo {
	a: string; // Access Token
	b: boolean; // Guest Login
}

export const token_information = persisted('xhnus', {
	a: '',
	b: false
} as AccessTokenInfo);

export const currentUserInformation = persisted(
	'clrsnus',
	{
		username: ''
	} as Profile,
	{
		storage: 'session'
	}
);

export const chooseModState = writable({
	moduleCode: '',
	lessonType: '',
	classNo: ''
} as LessonInfo);

export const searchTerm = writable('');

export interface LessonInfo {
	moduleCode: string;
	lessonType: string;
	classNo: string;
	colour: string;
}
