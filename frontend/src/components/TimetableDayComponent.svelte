<script lang="ts">
	import { chooseModState, currentlySelectedMods } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';
	import { modifyModEntry } from '../utils/format_db_information';
	interface TimetableDayProps {
		timeTableDayInfo: TimeTableDayInfo;
		semester: number;
		acadYear: string;
	}
	const { timeTableDayInfo, acadYear, semester }: TimetableDayProps = $props();

	const spaceAllowedToUse = $derived(100.0 / timeTableDayInfo.outerGroupLength);
	const startingOuterOffset = $derived(timeTableDayInfo.outerGroupIndex * spaceAllowedToUse);
	const leftMarginPercentage = $derived(
		timeTableDayInfo.innerGroupIndex * (spaceAllowedToUse / timeTableDayInfo.innerGroupLength) +
			startingOuterOffset
	);
	const showModName = $state(false);
	const width = $derived(spaceAllowedToUse / timeTableDayInfo.innerGroupLength);
</script>

<button
	style:margin-left="{leftMarginPercentage}%"
	style:width="{width}%;"
	class="absolute
	{timeTableDayInfo.isAChoiceSelection ? 'opacity-30' : 'opacity-100'}
	mt-{timeTableDayInfo.normalisedStartDuration * 192} h-{timeTableDayInfo.normalisedEndDuration *
		192 -
		timeTableDayInfo.normalisedStartDuration *
			192} border bg-accent text-xs wrap-break-word text-base-content"
	onclick={async () => {
		if (chooseModState.lessonType === '') {
			chooseModState.lessonType = timeTableDayInfo.lessonSchedule.lessonType;
			chooseModState.moduleCode = timeTableDayInfo.moduleCode;
			chooseModState.classNo = timeTableDayInfo.lessonSchedule.classNo;
		} else {
			$currentlySelectedMods = await modifyModEntry(
				$currentlySelectedMods,
				acadYear,
				semester,
				'you',
				'test',
				timeTableDayInfo.lessonSchedule.classNo,
				chooseModState
			);
			chooseModState.lessonType = '';
			chooseModState.moduleCode = '';
			chooseModState.classNo = '';
		}
	}}
>
	<div>{timeTableDayInfo.moduleCode} {showModName ? timeTableDayInfo.moduleName : ''}</div>

	{timeTableDayInfo.lessonSchedule.lessonType} [{timeTableDayInfo.lessonSchedule.classNo}]
</button>
