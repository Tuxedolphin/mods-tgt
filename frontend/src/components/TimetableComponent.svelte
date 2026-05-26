<script lang="ts">
	import TimetableDayComponent from './TimetableWeekComponent.svelte';
	import type { TimeTable } from '../types/mod_summaries';

	import { chooseModState } from '../shared/shared.svelte';
	import { filterTimetableByDay, queryAvailableLessons } from '../utils/format_db_information';

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
			<TimetableDayComponent
				timetableDayDisplayInfo={timetableDayInfo.flat()}
				{day}
				{acadYear}
				{semester}
			></TimetableDayComponent>
		{/await}
	{/each}
</div>
