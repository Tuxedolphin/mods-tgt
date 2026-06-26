<script lang="ts">
	import { chooseModState } from '$lib/shared/shared.svelte';
	import type { TimetableResponse } from '$lib/types/db_raw_types';
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { queryAvailableLessons } from '$lib/utils/format_db_information';
	import { onDestroy, onMount } from 'svelte';
	import type { Unsubscriber } from 'svelte/store';

	// const currentDay = new Date().getDay();
	// Get Localised Sunday to Sat:
	const daysOfWeek = [
		new Date('25 Jan 2026'), // Sunday
		new Date('26 Jan 2026'),
		new Date('27 Jan 2026'),
		new Date('28 Jan 2026'),
		new Date('29 Jan 2026'),
		new Date('30 Jan 2026'),
		new Date('31 Jan 2026') // Monday
	];

	const { timetables, acadYear, semester, timetable_id, timetable_name }: DaysOfWeekProp = $props();

	interface DaysOfWeekProp {
		timetables: TimetableResponse[];
		acadYear: string;
		semester: number;

		timetable_id: string;
		timetable_name: string;
	}

	const dateFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'short' });

	function formatShortDate(date: Date): string {
		return dateFormatter.format(date);
	}

	let chooseModStateCleanUpFunction: Unsubscriber;

	let lmao: { [day: number]: TimeTableDayInfo[] } = $state({});
	let length_of_lmao = $derived(Object.keys(lmao).length);
	onMount(() => {
		chooseModStateCleanUpFunction = chooseModState.subscribe(async (chooseModState) => {
			lmao = {};
			for (let day = 0; day < 5; day++) {
				let mods = await queryAvailableLessons(day, semester, acadYear, chooseModState);

				if (mods.length != 0) {
					lmao[day] = mods;
				}
			}
		});
	});

	onDestroy(() => {
		chooseModStateCleanUpFunction();
	});
</script>

{#if length_of_lmao != 0}
	<div class="grid grid-cols-{length_of_lmao} gap-0 text-center">
		{#each Object.entries(lmao) as [day, tt_info] (day)}
			<div class="flex h-6 items-center bg-base-200 text-xs">
				<p class="w-full text-center">{formatShortDate(daysOfWeek[Number.parseInt(day) + 1])}</p>
			</div>
		{/each}
	</div>
{:else}
	<div class="grid grid-cols-5 gap-0">
		{#each { length: 5 }, day}
			<div class="flex h-6 items-center bg-base-200 text-xs">
				<p class="w-full text-center">{formatShortDate(daysOfWeek[day + 1])}</p>
			</div>
		{/each}
	</div>
{/if}
