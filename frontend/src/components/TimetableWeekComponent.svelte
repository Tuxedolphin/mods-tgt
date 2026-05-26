<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import type { TimeTableDayInfo } from '../types/internal';
	import { findOverlappingTimeInfo } from '../utils/format_db_information';

	interface DisplayInfo {
		timetableDayDisplayInfo: TimeTableDayInfo[];
		day: number;
		semester: number;
		acadYear: string;
	}
	const { timetableDayDisplayInfo, day, acadYear, semester }: DisplayInfo = $props();

	const filteredInformation: TimeTableDayInfo[] = $derived(
		findOverlappingTimeInfo(timetableDayDisplayInfo)
	);
</script>

<div class="relative col-start-{day + 1} row-start-1">
	{#each filteredInformation as timetableDayInfo (timetableDayInfo)}
		<TimetableDayComponent timeTableDayInfo={timetableDayInfo} {acadYear} {semester}
		></TimetableDayComponent>
	{/each}
</div>
