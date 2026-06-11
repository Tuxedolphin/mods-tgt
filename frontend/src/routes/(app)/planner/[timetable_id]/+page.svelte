<script lang="ts">
	import DaysOfWeekHeader from '$lib/components/DaysOfWeekHeader.svelte';
	import SearchBar from '$lib/components/SearchBar.svelte';
	import Timeline from '$lib/components/Timeline.svelte';
	import TimetableComponent from '$lib/components/TimetableComponent.svelte';
	import { currentlySelectedMods, token_information } from '$lib/shared/shared.svelte';
	import { getTimetable } from '$lib/utils/format_db_information';
	import { onDestroy, onMount } from 'svelte';

	let is_timetable_loaded = $state(false);
	let timetable_metadata: TimetableWithMetadata = $state({
		academicYear: '',
		createdAt: '',
		id: '',
		metaData: [],
		name: '',
		semester: 0
	});

	let currentTimetableDisplay = $derived(
		getTimetable(
			timetable_metadata.academicYear,
			timetable_metadata.semester,
			$currentlySelectedMods
		)
	);

	import type { PageProps } from './$types';
	import { get_timetable_by_id, put_timetable_by_id } from '$lib/utils/db_operations';
	import type { TimetableWithMetadata } from '$lib/types/db_raw_types';
	import type { Unsubscriber } from 'svelte/store';
	import { format_semester_name } from '$lib/utils/formatting_utils';
	import ModListGroup from '$lib/components/ModListGroup.svelte';
	import { CircleX } from '@lucide/svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	let { params }: PageProps = $props();
	let unsubscribe_from_mods_list: Unsubscriber;
	onMount(async () => {
		is_timetable_loaded = false;
		const timetable_data = await get_timetable_by_id($token_information.a, params.timetable_id);

		if (timetable_data.isOk()) {
			timetable_metadata = timetable_data.value;
			$currentlySelectedMods = [timetable_data.value];

			unsubscribe_from_mods_list = currentlySelectedMods.subscribe(
				async (updated_timetable) => {
					for (const timetable of updated_timetable) {
						if (timetable.id == params.timetable_id) {
							const response = await put_timetable_by_id(
								$token_information.a,
								timetable.id,
								timetable
							);
							if (response.isOk()) {
								console.log('Update for Timetable ' + timetable.id);
							}
						}
					}
				},
				() => {}
			);

			is_timetable_loaded = true;
		}
	});

	onDestroy(() => {
		if (unsubscribe_from_mods_list) {
			unsubscribe_from_mods_list();
		}
	});
</script>

{#if is_timetable_loaded}
	<div class="flex justify-between">
		<div class="flex items-center gap-4">
			<CircleX
				onclick={() => {
					goto(resolve('/(app)/home'));
				}}
			></CircleX>
			<h2 class="text-2xl font-bold">{timetable_metadata.name}</h2>
		</div>

		<h2 class="text-2xl">
			AY{timetable_metadata.academicYear} - {format_semester_name(timetable_metadata.semester)}
		</h2>
	</div>

	<SearchBar
		timetable_id={timetable_metadata.id}
		timetable_name={timetable_metadata.name}
		acadYear={timetable_metadata.academicYear}
		semester={timetable_metadata.semester}
	></SearchBar>

	<div class="flex">
		<Timeline></Timeline>
		<div class="flex-1 flex-col">
			<DaysOfWeekHeader
				timetable_id={timetable_metadata.id}
				timetable_name={timetable_metadata.name}
				timetables={currentTimetableDisplay}
				acadYear={timetable_metadata.academicYear}
				semester={timetable_metadata.semester}
			></DaysOfWeekHeader>
			<TimetableComponent
				timetable_id={timetable_metadata.id}
				timetable_name={timetable_metadata.name}
				timetables={currentTimetableDisplay}
				acadYear={timetable_metadata.academicYear}
				semester={timetable_metadata.semester}
			></TimetableComponent>
		</div>
	</div>

	<ModListGroup acadYear={timetable_metadata.academicYear} timetables={currentTimetableDisplay}
	></ModListGroup>
{/if}
