<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { timetable_list_should_be_refreshed } from '$lib/shared/shared.svelte';
	import type { TimetableInfo } from '$lib/types/db_raw_types';
	import { delete_timetable_by_id } from '$lib/utils/db_operations';
	import { format_semester_name } from '$lib/utils/text_formatting';

	interface TimeTableCardComponentProps {
		timetable: TimetableInfo;
		access_token: string;
	}

	const { timetable, access_token }: TimeTableCardComponentProps = $props();
</script>

<div class="card w-full bg-base-300 card-border">
	<div class="card-body">
		<h2 class="card-title">{timetable.name}</h2>
		<div class="flex justify-between">
			<div>AY{timetable.academicYear}</div>
			<div>{format_semester_name(timetable.semester)}</div>
		</div>
		<p class="font-bold"></p>

		<div class="card-actions justify-end">
			<button
				class="btn btn-error"
				onclick={async () => {
					await delete_timetable_by_id(access_token, timetable.id);
					timetable_list_should_be_refreshed.set(true);
				}}>Delete</button
			>
			<button
				class="btn btn-primary"
				onclick={() =>
					goto(resolve('/(app)/planner/[timetable_id]', { timetable_id: timetable.id }))}
				>Open</button
			>
		</div>
	</div>
</div>
