import type { TimetableWithMetadata } from '$lib/types/db_raw_types';
import { persisted } from 'svelte-persisted-store';
import { writable } from 'svelte/store';
// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted('selectedMods', [] as TimetableWithMetadata[]);

export const preferences = persisted('prefs', {
	currentSemView: 2,
	acadYear: '2025-2026'
});

interface UserInfo {
	displayName: string;
}

export const registered = writable(false);
export const timetable_list_should_be_refreshed = writable(false);

interface AccessTokenInfo {
	access_token: string;
	is_guest_login: boolean;
}

export const access_token = persisted('ac:tok', {
	access_token: '',
	is_guest_login: false
} as AccessTokenInfo);

export const currentUserInformation = persisted('user', {} as UserInfo);

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
	colour: string;
}
