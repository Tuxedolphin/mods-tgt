<script lang="ts">
	import { chooseModState } from '$lib/shared/shared.svelte';
	import type { TimeTable } from '$lib/types/mod_summaries';
	import { filterTimetableByDay, queryAvailableLessons } from '$lib/utils/format_db_information';
	import TimetableWeekComponent from './TimetableWeekComponent.svelte';

	const heightOfOneHourLessonPx = 16;

	interface Timetables {
		timetables: TimeTable[];
		acadYear: string;
		semester: number;
	}
	const { timetables, acadYear, semester }: Timetables = $props();
</script>

<div class="grid grid-cols-5 grid-rows-12">
	{#each { length: 12 }, y}
		{#each { length: 5 }, x}
			<div
				class="col-start-{x + 1} row-start-{y + 1} h-{heightOfOneHourLessonPx} {y % 2 == 0
					? 'bg-base-300'
					: 'bg-base-200'}"
			></div>
		{/each}
	{/each}
	{#each { length: 5 }, day}
		{#await Promise.all( [filterTimetableByDay(day, timetables), queryAvailableLessons(day, semester, acadYear, chooseModState)] ) then timetableDayInfo}
			<TimetableWeekComponent
				timetableDayDisplayInfo={timetableDayInfo.flat()}
				{day}
				{acadYear}
				{semester}
			></TimetableWeekComponent>
		{/await}
	{/each}
</div>
