<script lang="ts">
	import { onMount } from 'svelte';
	import type { TimeTableDayInfo } from '../types/internal';

	const {
		lessonSchedule,
		moduleName,
		moduleCode,
		normalisedStartDuration,
		normalisedEndDuration,
		searchedModuleCodes
	}: TimeTableDayInfo = $props();
	const currentIndex = $derived(
		[...searchedModuleCodes].sort().indexOf(moduleCode) * (100 / searchedModuleCodes.size)
	);
</script>

<div
	style="margin-left: {currentIndex}%"
	class="absolute
	mt-{normalisedStartDuration * 192} w-1/{searchedModuleCodes.size} h-{normalisedEndDuration * 192 -
		normalisedStartDuration * 192} bg-amber-100 text-xs wrap-break-word"
>
	{moduleCode} - {lessonSchedule.startTime} to {lessonSchedule.endTime}
</div>
