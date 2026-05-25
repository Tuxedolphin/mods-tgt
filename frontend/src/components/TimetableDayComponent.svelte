<script lang="ts">
	import { chooseModState, currentlySelectedMods, preferences } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';

	const {
		lessonSchedule,
		moduleName,
		moduleCode,
		normalisedStartDuration,
		normalisedEndDuration,
		isAChoiceSelection,
		innerGroupIndex,
		innerGroupLength,
		outerGroupIndex,
		outerGroupLength
	}: TimeTableDayInfo = $props();

	const spaceAllowedToUse = $derived(100.0 / outerGroupLength);
	const startingOuterOffset = $derived(outerGroupIndex * spaceAllowedToUse);
	const leftMarginPercentage = $derived(
		innerGroupIndex * (spaceAllowedToUse / innerGroupLength) + startingOuterOffset
	);
	const showModName = $state(true);
	const width = $derived(spaceAllowedToUse / innerGroupLength);
</script>

<button
	style:margin-left="{leftMarginPercentage}%"
	style:width="{width}%;"
	class="absolute
	{isAChoiceSelection ? 'opacity-30' : 'opacity-100'}
	mt-{normalisedStartDuration * 192} h-{normalisedEndDuration * 192 -
		normalisedStartDuration * 192} border bg-accent text-xs wrap-break-word text-base-content"
	onclick={() => {
		if (chooseModState.lessonType === '') {
			chooseModState.lessonType = lessonSchedule.lessonType;
			chooseModState.moduleCode = moduleCode;
			chooseModState.classNo = lessonSchedule.classNo;
		} else {
			
			$currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView][moduleCode][lessonSchedule.lessonType] =
				lessonSchedule.classNo;
			chooseModState.lessonType = '';
			chooseModState.moduleCode = '';
			chooseModState.classNo = '';
		}
	}}
>
	<div>{moduleCode} {showModName ? moduleName : ''}</div>

	{lessonSchedule.lessonType} [{lessonSchedule.classNo}]
</button>
