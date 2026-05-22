<script lang="ts">
	import type { TimeTableDayInfo } from '../types/internal';

	const {
		lessonSchedule,
		moduleName,
		moduleCode,
		normalisedStartDuration,
		normalisedEndDuration,
		searchedModuleCodes,
		isAChoiceSelection,
		uniqueIdentifer
	}: TimeTableDayInfo = $props();
	const currentIndex = $derived(
		[...searchedModuleCodes].sort().indexOf(uniqueIdentifer) * (100 / searchedModuleCodes.size)
	);
</script>

<button
	style="margin-left: {currentIndex}%"
	class="absolute
	{isAChoiceSelection ? 'opacity-30' : 'opacity-100'}
	mt-{normalisedStartDuration * 192} w-1/{searchedModuleCodes.size} h-{normalisedEndDuration * 192 -
		normalisedStartDuration * 192} border bg-amber-100 text-xs wrap-break-word"
	onclick={() => console.log('Hello')}
>
	{moduleCode}
	{lessonSchedule.classNo}
	{JSON.stringify([...searchedModuleCodes])}
</button>
