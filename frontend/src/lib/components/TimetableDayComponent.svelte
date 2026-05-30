<script lang="ts">
	import { currentlySelectedMods, chooseModState } from '$lib/shared/shared.svelte';
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { modifyModEntry } from '$lib/utils/format_db_information';

	interface TimetableDayProps {
		timeTableDayInfo: TimeTableDayInfo;
		semester: number;
		acadYear: string;
		timetable_id: string;
		timetable_name: string;
	}
	const { timeTableDayInfo, acadYear, semester, timetable_id, timetable_name }: TimetableDayProps =
		$props();

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
			192} border bg-red-400 text-xs wrap-break-word text-black"
	onclick={async () => {
		if (chooseModState.lessonType === '') {
			chooseModState.lessonType = timeTableDayInfo.lessonSchedule.lessonType;
			chooseModState.moduleCode = timeTableDayInfo.moduleCode;
			chooseModState.classNo = timeTableDayInfo.lessonSchedule.classNo;
		} else {
			currentlySelectedMods.set(
				await modifyModEntry(
					$currentlySelectedMods,
					acadYear,
					semester,
					timetable_id,
					timetable_name,
					timeTableDayInfo.moduleCode,
					timeTableDayInfo.lessonSchedule.lessonType,
					timeTableDayInfo.lessonSchedule.classNo,
					chooseModState
				)
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
