<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import type { TimeTableDayInfo } from '../types/internal';

	interface DisplayInfo {
		timetableDayDisplayInfo: TimeTableDayInfo[];
		day: number;
	}
	const { timetableDayDisplayInfo, day }: DisplayInfo = $props();

	const filteredInformation: TimeTableDayInfo[] = $derived(
		findOverlappingTimeInfoNew(timetableDayDisplayInfo)
	);

	function findOverlappingTimeInfoNew(allTime: TimeTableDayInfo[]): TimeTableDayInfo[] {
		const groupTimes = Object.groupBy(allTime, (x) => x.normalisedStartDuration);
		const MAX_ITER = 1000;
		let iterIdx = 0;
		const processedTimings: string[] = [];
		const processedGroups: {
			[key: number]: TimeTableDayInfo[][];
		} = {};
		let lengthOfGroups = 0;
		// eslint-disable-next-line @typescript-eslint/no-unused-vars
		for (const _len in groupTimes) lengthOfGroups++;
		let groupId = 0;
		while (processedTimings.length != lengthOfGroups && iterIdx != MAX_ITER) {
			iterIdx++;
			let firstGroup: TimeTableDayInfo[] = [];
			let firstGroupProcess: string = '';
			for (const i in groupTimes) {
				if (processedTimings.includes(i)) continue;
				firstGroup = groupTimes[i]!;
				firstGroupProcess = i;
				firstGroup.sort(
					(a, b) =>
						b.normalisedEndDuration -
						b.normalisedStartDuration -
						(a.normalisedEndDuration - a.normalisedStartDuration)
				);

				processedTimings.push(i);

				if (!processedGroups[groupId]) processedGroups[groupId] = [];
				processedGroups[groupId].push(firstGroup);
				break;
			}

			if (!firstGroup) break;
			let endTime = firstGroup[0].normalisedEndDuration;

			// Find groups:
			for (const i in groupTimes) {
				if (i == firstGroupProcess) continue;
				const group = groupTimes[i]![0];
				if (group.normalisedStartDuration >= endTime) {
					endTime = group.normalisedEndDuration;
					processedTimings.push(i);
					processedGroups[groupId].push(groupTimes[i]!);
				}
			}

			// Find Groups that may not exactly match endtimes:

			groupId++;
		}

		const outerGroupLength = Object.keys(processedGroups).length;

		for (const group in processedGroups) {
			const outerGroupIdx = Number.parseInt(group);

			for (const lessonGrouping of processedGroups[group]) {
				const innerGroupLength = lessonGrouping.length;
				for (let i = 0; i < lessonGrouping.length; i++) {
					const lesson = lessonGrouping[i];
					lesson.outerGroupIndex = outerGroupIdx;
					lesson.outerGroupLength = outerGroupLength;
					lesson.innerGroupLength = innerGroupLength;
					lesson.innerGroupIndex = i;
				}
			}
		}

		if (iterIdx == MAX_ITER) {
			console.log('Unable to find pairings');
		}

		return allTime;
	}
</script>

<div class="relative col-start-{day + 1} row-start-1">
	{#each filteredInformation as mod (mod)}
		<TimetableDayComponent
			lessonSchedule={mod.lessonSchedule}
			moduleCode={mod.moduleCode}
			moduleName={mod.moduleName}
			normalisedStartDuration={mod.normalisedStartDuration}
			normalisedEndDuration={mod.normalisedEndDuration}
			isAChoiceSelection={mod.isAChoiceSelection}
			innerGroupLength={mod.innerGroupLength}
			innerGroupIndex={mod.innerGroupIndex}
			outerGroupIndex={mod.outerGroupIndex}
			outerGroupLength={mod.outerGroupLength}
		></TimetableDayComponent>
	{/each}
</div>
