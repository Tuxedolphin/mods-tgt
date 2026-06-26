<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { token_information } from '$lib/shared/shared.svelte';
	import { create_empty_timetable } from '$lib/utils/db_operations';
	import { format_semester_name } from '$lib/utils/formatting_utils';
	import { CirclePlus } from '@lucide/svelte';
	import GenericDialog from '../GenericDialog.svelte';
	import { roomHub } from '$lib/stores/roomHub';
	import type { TimetablePostTemplate } from '$lib/types/db_raw_types';

	let dialog: HTMLDialogElement;
	let timetable_name = $state('');
	let semester_number = $state(1);
	let academic_year = $state('2025-2026');

	async function create_new_empty_timetable() {
		const timetable_info = await create_empty_timetable(
			$token_information.a,
			timetable_name,
			semester_number,
			academic_year
		);

		if (timetable_info.isOk()) {
			dialog.close();
			goto(resolve('/(app)/planner/[timetable_id]', { timetable_id: timetable_info.value.id }));
		}
	}
</script>

<CirclePlus size={32} class="cursor-pointer" onclick={() => dialog.show()}></CirclePlus>
<!-- Open the modal using ID.showModal() method -->

<GenericDialog bind:dialog>
	<h3 class="text-lg font-bold">Create new timetable</h3>
	<p class="py-4">Name your timetable:</p>
	<input class="input" bind:value={timetable_name} />

	<p class="py-4">Choose AY:</p>
	<select class="select" bind:value={academic_year}>
		<option>2025-2026</option>
		<option>2024-2025</option>
	</select>

	<p class="py-4">Choose Semester:</p>
	<select class="select" bind:value={semester_number}>
		{#each { length: 4 }, i}
			<option value={i + 1}>{format_semester_name(i + 1)}</option>
		{/each}
	</select>

	<div class="modal-action">
		<!-- if there is a button in form, it will close the modal -->
		<button class="btn btn-primary" onclick={() => create_new_empty_timetable()}
			>Create timetable</button
		>
		<button class="btn btn-error" onclick={() => dialog.close()}>Cancel</button>
	</div>
</GenericDialog>
