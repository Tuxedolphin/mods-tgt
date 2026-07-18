import { writable } from 'svelte/store'
import { persisted } from 'svelte-persisted-store'
import type { Profile, TimetableDetailedResponse } from '$lib/types/db_raw_types'
// First param `preferences` is the local storage key.
// Second param is the initial value.
export const currentlySelectedMods = persisted('selectedMods', [] as TimetableDetailedResponse[], {
	storage: 'session'
})

export const timetable_list_should_be_refreshed = writable(false)

interface AccessTokenInfo {
	a: string // Access Token
	b: boolean // Guest Login
}

export const token_information = persisted('xhnus', {
	a: '',
	b: false
} as AccessTokenInfo)

export const currentUserInformation = persisted('clrsnus', {
	userId: '',
	username: null,
	avatarUrl: null,
	handle: null
} as Profile)

export const currentWorkingTimetable = persisted(
	'erixnus',
	{
		timetable_id: ''
	},
	{
		storage: 'session'
	}
)

export const chooseModState = writable({
	moduleCode: '',
	lessonType: '',
	classNo: '',
	colour: '',
	selectedTimetableId: ''
} as LessonInfo)

export const searchTerm = writable('')

export interface LessonInfo {
	moduleCode: string
	lessonType: string
	classNo: string
	colour: string
	selectedTimetableId: string
}
