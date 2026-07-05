<script lang="ts">
	import { Import } from '@lucide/svelte';
	import GenericDialog from '../../routes/(app)/GenericDialog.svelte';
	import { parse_mods_link } from '$lib/utils/nusmods_parser';

	// svelte-ignore non_reactive_update
	let dialog: HTMLDialogElement;
	let timetable_name = $state('');
	let semester_number = $state(1);
	let academic_year = $state('2026-2027');
	let share_link = $state('');

	async function create_new_empty_timetable() {
		parse_mods_link(share_link);
		// const timetable_info = await create_empty_timetable(
		// 	$token_information.a,
		// 	timetable_name,
		// 	semester_number,
		// 	academic_year
		// );

		// if (timetable_info.isOk()) {
		// 	dialog.close();
		// 	goto(resolve('/(app)/planner/[timetable_id]', { timetable_id: timetable_info.value.id }));
		// }
	}
</script>

<Import size={32} class="cursor-pointer" onclick={() => dialog.show()}></Import>
<!-- Open the modal using ID.showModal() method -->

<GenericDialog bind:dialog closeHandler={() => {}}>
	<h3 class="text-lg font-bold">Import from NUSMods!</h3>
	<p class="py-4">Name your timetable:</p>
	<input class="input" bind:value={timetable_name} />

	<p class="py-4">Choose AY (Make sure your AY Matches the one in NUSMods!)</p>
	<select class="select" bind:value={academic_year}>
		<option>2026-2027</option>
		<option>2025-2026</option>
		<option>2024-2025</option>
	</select>

	<p class="py-4">Paste NUS Mods Share Link:</p>
	<input class="input" bind:value={share_link} />

	<div class="modal-action">
		<!-- if there is a button in form, it will close the modal -->
		<button class="btn btn-primary" onclick={() => create_new_empty_timetable()}
			>Create timetable</button
		>
		<button class="btn btn-error" onclick={() => dialog.close()}>Cancel</button>
	</div>
</GenericDialog>
