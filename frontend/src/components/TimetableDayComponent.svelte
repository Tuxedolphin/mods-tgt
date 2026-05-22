<script lang="ts">
	import { chooseModState } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';

	const {
		lessonSchedule,
		moduleName,
		moduleCode,
		normalisedStartDuration,
		normalisedEndDuration,
		isAChoiceSelection,
		groupIndex,
		groupLength
	}: TimeTableDayInfo = $props();
</script>

<button
	style="margin-left: {groupIndex * (100.0 / groupLength)}%;"
	class="absolute
	{isAChoiceSelection ? 'opacity-30' : 'opacity-100'}
	mt-{normalisedStartDuration * 192} w-1/{groupLength} h-{normalisedEndDuration * 192 -
		normalisedStartDuration * 192} border bg-amber-100 text-xs wrap-break-word"
	onclick={() => {
		chooseModState.lessonType = lessonSchedule.lessonType;
		chooseModState.moduleCode = moduleCode;
	}}
>
	{moduleCode} - {moduleName} - {lessonSchedule.lessonType}
</button>
