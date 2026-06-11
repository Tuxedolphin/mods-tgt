<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { timetable_list_should_be_refreshed } from '$lib/shared/shared.svelte';
	import type { TimetableInfo } from '$lib/types/db_raw_types';
	import { delete_timetable_by_id } from '$lib/utils/db_operations';
	import { format_semester_name } from '$lib/utils/formatting_utils';
	import { EllipsisVertical } from '@lucide/svelte';
	import GenericDialog from '../../routes/(app)/GenericDialog.svelte';

	interface TimeTableCardComponentProps {
		timetable: TimetableInfo;
		access_token: string;
	}
	let dialog: HTMLDialogElement;
	let selected_timetable_name = $state('');
	let selected_timetable_id = $state('');
	const { timetable, access_token }: TimeTableCardComponentProps = $props();
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	class="card min-w-0 cursor-pointer bg-base-300 card-sm card-border"
	onclick={() => goto(resolve('/(app)/planner/[timetable_id]', { timetable_id: timetable.id }))}
>
	<div class="card-body">
		<div class="flex items-center justify-between">
			<h2 class="truncate text-2xl font-semibold">{timetable.name}</h2>

			<button
				popovertarget="option-{timetable.id}"
				onclick={(e) => {
					e.stopPropagation();
				}}
				style="anchor-name:--option-{timetable.id}"
			>
				<EllipsisVertical></EllipsisVertical>
			</button>
		</div>
		<div class="flex justify-between">
			<div>AY{timetable.academicYear}</div>
			<div>{format_semester_name(timetable.semester)}</div>
		</div>
		<p class="font-bold"></p>

		<div class="card-actions justify-end"></div>
	</div>
</div>

<ul
	class="menu dropdown dropdown-left w-52 rounded-box bg-base-100 shadow-sm"
	popover
	id="option-{timetable.id}"
	style="position-anchor:--option-{timetable.id}"
>
	<li>
		<a
			onclick={async () => {
				selected_timetable_name = timetable.name;
				selected_timetable_id = timetable.id;
				dialog.show();
			}}>Delete</a
		>
	</li>
</ul>

<GenericDialog bind:dialog>
	<h3 class="text-lg font-bold">Confirm?</h3>
	<p class="py-4">
		Delete the timetable: '{selected_timetable_name}' (this action is irreversible!)
	</p>

	<div class="modal-action">
		<button
			class="btn btn-primary"
			onclick={async () => {
				await delete_timetable_by_id(access_token, timetable.id);
				timetable_list_should_be_refreshed.set(true);
			}}>Delete!</button
		>
		<button class="btn btn-error" onclick={() => dialog.close()}>Cancel</button>
	</div>
</GenericDialog>
