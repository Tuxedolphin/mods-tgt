<script lang="ts">
	import { currentlySelectedMods } from '$lib/shared/shared.svelte';
	import type { TimetableLessonMetadata, TimetableWithMetadata } from '$lib/types/db_raw_types';
	import {
		modifyModColour,
		modifyModEntry,
		removeModEntry
	} from '$lib/utils/format_db_information';
	import { colours } from '$lib/utils/formatting_utils';
	import { X } from '@lucide/svelte';
	interface ModListModInfoProps {
		timetable: TimetableWithMetadata;
	}
	let { timetable }: ModListModInfoProps = $props();
	let lesson_groups = $derived(Object.groupBy(timetable.metaData, (x) => x.moduleCode));
	let lesson_headers = $derived(Object.keys(lesson_groups));
	let dialog: HTMLDialogElement;
	let selectedLessonGroup: TimetableLessonMetadata[] = $state([]);
</script>

{#each lesson_headers as lesson (lesson)}
	<div class="card w-full bg-base-300 card-border">
		<div class="flex justify-between p-4">
			<div class="flex gap-2">
				<!-- svelte-ignore a11y_consider_explicit_label -->
				<button
					onclick={() => {
						selectedLessonGroup = lesson_groups[lesson]!;
						dialog.showModal();
					}}
					class="flex-initial {lesson_groups[lesson]![0].colour} badge"
				></button>
				<div class="flex-none">{lesson}</div>
			</div>

			<X
				onclick={() => {
					currentlySelectedMods.set(
						removeModEntry(
							$currentlySelectedMods,
							timetable.academicYear,
							timetable.semester,
							timetable.id,
							timetable.name,
							lesson_groups[lesson]![0].moduleCode
						)
					);
				}}
			></X>
		</div>
	</div>
{/each}

<dialog bind:this={dialog} class="modal">
	<div class="modal-box">
		<h3 class="text-lg font-bold">Change Colour</h3>
		{#each colours as colour (colour)}
			<button
				onclick={() => {
					currentlySelectedMods.set(
						modifyModColour(
							$currentlySelectedMods,
							timetable.academicYear,
							timetable.semester,
							timetable.id,
							timetable.name,
							selectedLessonGroup[0].moduleCode,
							colour
						)
					);
				}}
				class="flex-initial {colour} badge badge-lg"
			></button>
		{/each}

		<div class="modal-action">
			<!-- if there is a button in form, it will close the modal -->
			<!-- <button class="btn btn-primary" onclick={() => create_new_empty_timetable()}
				>Create timetable</button
			> -->
			<button class="btn btn-error" onclick={() => dialog.close()}>Cancel</button>
		</div>
	</div>
	<form method="dialog" class="modal-backdrop">
		<button>close</button>
	</form>
</dialog>
