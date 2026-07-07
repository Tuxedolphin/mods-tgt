<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { Unsubscriber } from 'svelte/store';
	import { chooseModState } from '$lib/shared/shared.svelte';
	import type { TimetableDetailedResponse } from '$lib/types/db_raw_types';
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { filterTimetableByDay, queryAvailableLessons } from '$lib/utils/format_db_information';
	import BackgroundTiles from './BackgroundTiles.svelte';
	import TimetableWeekComponent from './TimetableWeekComponent.svelte';

	const height_of_one_hour_lesson = 18;

	interface TimetablesProps {
		timetables: TimetableDetailedResponse[];
		acadYear: string;
		semester: number;
	}

	const max_hours_displayed = 12;
	const max_days_displayed = 5;

	const { timetables, acadYear, semester }: TimetablesProps = $props();
	onDestroy(() => {
		$chooseModState = {
			classNo: '',
			colour: '',
			lessonType: '',
			moduleCode: '',
			selectedTimetableId: ''
		};

		chooseModStateCleanUpFunction();
	});

	let chooseModStateCleanUpFunction: Unsubscriber;

	let lmao: { [day: number]: TimeTableDayInfo[] } = $state({});

	onMount(() => {
		chooseModStateCleanUpFunction = chooseModState.subscribe(async (chooseModState) => {
			lmao = {};
			for (let day = 0; day < max_days_displayed; day++) {
				let mods = await queryAvailableLessons(day, semester, acadYear, chooseModState);

				if (mods.length != 0) {
					lmao[day] = mods;
				}
			}
		});
	});
</script>

{#if Object.keys(lmao).length != 0}
	<div class="grid grid-cols-{Object.keys(lmao).length} grid-rows-12">
		{#each { length: max_hours_displayed }, y}
			{#each { length: Object.keys(lmao).length }, x}
				<BackgroundTiles
					{max_hours_displayed}
					x_cord={x}
					y_cord={y}
					heightOfOneHourLessonPx={height_of_one_hour_lesson}
				></BackgroundTiles>
			{/each}
		{/each}
		{#each Object.entries(lmao) as [day, tt_info], idx (day)}
			{#await Promise.all( [filterTimetableByDay(Number.parseInt(day), timetables), $state.snapshot(tt_info)] ) then timetableDayInfo}
				<TimetableWeekComponent
					{height_of_one_hour_lesson}
					timetableDayDisplayInfo={timetableDayInfo.flat()}
					day={idx}
					{acadYear}
					{semester}
				></TimetableWeekComponent>
			{/await}
		{/each}
	</div>
{:else}
	<div class="grid grid-cols-5 grid-rows-12">
		{#each { length: max_hours_displayed }, y}
			{#each { length: max_days_displayed }, x}
				<BackgroundTiles
					{max_hours_displayed}
					x_cord={x}
					y_cord={y}
					heightOfOneHourLessonPx={height_of_one_hour_lesson}
				></BackgroundTiles>
			{/each}
		{/each}
		{#each { length: max_days_displayed }, day}
			{#await Promise.all( [filterTimetableByDay(day, timetables), queryAvailableLessons(day, semester, acadYear, $chooseModState)] ) then timetableDayInfo}
				<TimetableWeekComponent
					{height_of_one_hour_lesson}
					timetableDayDisplayInfo={timetableDayInfo.flat()}
					{day}
					{acadYear}
					{semester}
				></TimetableWeekComponent>
			{/await}
		{/each}
	</div>
{/if}
