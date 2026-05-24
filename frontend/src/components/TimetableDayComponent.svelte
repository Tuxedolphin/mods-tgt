<script lang="ts">
	import { chooseModState, currentlySelectedMods } from '../shared/shared.svelte';
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
		normalisedStartDuration * 192} border bg-primary-content text-xs wrap-break-word"
	onclick={() => {
		if (chooseModState.lessonType === '') {
			chooseModState.lessonType = lessonSchedule.lessonType;
			chooseModState.moduleCode = moduleCode;
			chooseModState.classNo = lessonSchedule.classNo;
		} else {
			$currentlySelectedMods.selectedMods[moduleCode][lessonSchedule.lessonType] =
				lessonSchedule.classNo;
			chooseModState.lessonType = '';
			chooseModState.moduleCode = '';
			chooseModState.classNo = '';
		}
	}}
>
	{moduleCode} - {moduleName} - {lessonSchedule.lessonType} [{lessonSchedule.classNo}]
</button>
