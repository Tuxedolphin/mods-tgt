<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import type { Module, RawLesson } from '../types/modules';
	import { currentlySelectedMods, chooseModState, type LessonInfo } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';
	import { normaliseDuration } from '../utils/calculations_for_ui';

	interface WeekTimeTabledComponent {
		day: number;
		modInfo: { [moduleCode: string]: Module };
	}
	const { day, modInfo }: WeekTimeTabledComponent = $props();
	const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

	const filteredInformation: TimeTableDayInfo[] = $derived(
		calculateOverlappingTimes(filterByDay(modInfo, chooseModState))
	);

	function findOverlappingTimeInfo(allTimes: TimeTableDayInfo[]) {
		let groupsFound = 0;
		let timeOut = 0;
		let timeOutLimit = 10_000;
		let groupTimes = [];

		while (groupsFound != allTimes.length && timeOut != timeOutLimit) {
			for (const item of allTimes) {
				if (item.hasFoundAGroup) continue;
				if (!item.hasFoundAGroup) {
					// try to match a group:
					for (const times of groupTimes) {
						const groupStartTime = times['startTime'];
						const groupEndTime = times['endTime'];
						const groupMembers = times['members'];

						// match group: add member:
						if (
							item.normalisedStartDuration >= groupStartTime &&
							item.normalisedEndDuration <= groupEndTime
						) {
							groupMembers.push(item.uniqueIdentifer);

							if (item.normalisedEndDuration >= groupEndTime) {
								times['endTime'] = item.normalisedEndDuration;
							}
							item.hasFoundAGroup = true;
							groupsFound++;
							break;
						}
					}
				}

				// unable to establish membership:
				if (!item.hasFoundAGroup) {
					groupTimes.push({
						startTime: item.normalisedStartDuration,
						endTime: item.normalisedEndDuration,
						members: [item.uniqueIdentifer]
					});
					groupsFound++;
					item.hasFoundAGroup = true;
				}
			}

			timeOut++;
		}

		if (timeOutLimit == timeOut) {
			console.log('Error Finding Group Pairing');
		}
		for (const groups of groupTimes) {
			for (let i = 0; i < groups.members.length; i++) {
				const element = groups.members[i];

				for (const allMods of allTimes) {
					if (allMods.uniqueIdentifer != element) continue;
					allMods.groupIndex = i;
					allMods.groupLength = groups.members.length;
				}
			}
		}
	}

	function calculateOverlappingTimes(timeTableInfo: TimeTableDayInfo[]): TimeTableDayInfo[] {
		findOverlappingTimeInfo(timeTableInfo);
		return timeTableInfo;
	}

	function filterByDay(
		modInfo: { [moduleCode: string]: Module },
		userState: LessonInfo
	): TimeTableDayInfo[] {
		const totalInfo: TimeTableDayInfo[] = [];
		// For Displaying the Timetable:
		for (const mod in modInfo) {
			const info = modInfo[mod];

			const weekData = info.semesterData.find((semNo) => semNo.semester == 2);
			const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);

			const selectedMod = $currentlySelectedMods.selectedMods[mod];

			for (const lessonType in selectedMod) {
				const classNo = selectedMod[lessonType];
				const lessonForDay = ttData?.filter(
					(x) => x.lessonType == lessonType && x.classNo == classNo
				);

				if (lessonForDay?.length != 0) {
					const lesson = lessonForDay![0] as RawLesson;
					const identifer = `${lesson.classNo}-${lesson.lessonType}-${info.moduleCode}`;
					totalInfo.push({
						lessonSchedule: lesson,
						moduleCode: info.moduleCode,
						moduleName: info.title,
						normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
						normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
						isAChoiceSelection: false,
						uniqueIdentifer: identifer,
						hasFoundAGroup: false,
						groupIndex: 0,
						groupLength: 0
					});
				}
			}
		}
		const lessonQuery = modInfo[userState.moduleCode];

		const weekData = lessonQuery?.semesterData.find((semNo) => semNo.semester == 2);
		const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);
		const lessonTypeToMatch = ttData?.filter((x) => x.lessonType == userState.lessonType);
		if (lessonTypeToMatch) {
			for (const lesson of lessonTypeToMatch!) {
				if (lesson.classNo == userState.classNo) continue;
				const identifer = `${lesson.classNo}-${lesson.lessonType}-${lessonQuery.moduleCode}`;
				totalInfo.push({
					lessonSchedule: lesson,
					moduleCode: lessonQuery.moduleCode,
					moduleName: lessonQuery.title,
					normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
					normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
					isAChoiceSelection: true,
					uniqueIdentifer: identifer,
					hasFoundAGroup: false,
					groupIndex: 0,
					groupLength: 0
				});
			}
		}

		totalInfo.sort(
			(a, b) =>
				b.normalisedEndDuration -
				b.normalisedStartDuration -
				(a.normalisedEndDuration - a.normalisedStartDuration)
		);

		return totalInfo;
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
			uniqueIdentifer={mod.uniqueIdentifer}
			groupIndex={mod.groupIndex}
			groupLength={mod.groupLength}
			hasFoundAGroup={mod.hasFoundAGroup}
		></TimetableDayComponent>
	{/each}
</div>
