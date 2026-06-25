<script lang="ts">
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { findOverlappingTimeInfo } from '$lib/utils/format_db_information';
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	interface DisplayInfo {
		timetableDayDisplayInfo: TimeTableDayInfo[];
		day: number;
		semester: number;
		acadYear: string;
		timetable_id: string;
		timetable_name: string;
		height_of_one_hour_lesson: number;
	}
	const {
		timetableDayDisplayInfo,
		day,
		acadYear,
		semester,
		timetable_id,
		timetable_name,
		height_of_one_hour_lesson
	}: DisplayInfo = $props();

	const filteredInformation: TimeTableDayInfo[] = $derived(
		findOverlappingTimeInfo(timetableDayDisplayInfo)
	);
</script>

<div class="relative col-start-{day + 1} row-start-1">
	{#each filteredInformation as timetableDayInfo (timetableDayInfo)}
		<TimetableDayComponent
			{timetable_id}
			{timetable_name}
			{height_of_one_hour_lesson}
			timetable_colour={timetableDayInfo.timetableColour}
			timeTableDayInfo={timetableDayInfo}
			{acadYear}
			{semester}
		></TimetableDayComponent>
	{/each}
</div>
