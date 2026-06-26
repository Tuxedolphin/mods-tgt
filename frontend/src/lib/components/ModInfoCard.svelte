<script lang="ts">
	import { currentlySelectedMods } from '$lib/shared/shared.svelte';
	import type { TimetableModule, TimetableResponse } from '$lib/types/db_raw_types';
	import { getFullModInfo } from '$lib/utils/fetch_from_cache';
	import { modifyModColour, removeModEntry } from '$lib/utils/format_db_information';
	import { X } from '@lucide/svelte';
	import GenericDialog from '../../routes/(app)/GenericDialog.svelte';
	import { colours } from '$lib/utils/formatting_utils';
	interface ModInfoCardProps {
		lesson_groups: Partial<Record<string, TimetableModule[]>>;
		acadYear: string;
		lesson_header: string;
		timetable: TimetableResponse;
	}
	let { lesson_groups, acadYear, lesson_header, timetable }: ModInfoCardProps = $props();
	let selectedLessonGroup: TimetableModule[] = $state([]);
	let dialog: HTMLDialogElement;
</script>

<div class="card w-full bg-base-300 card-border">
	<details class="collapse-arrow collapse h-full">
		<summary class="collapse-title h-full">
			<div class="flex">
				<div class="flex h-20 w-full items-center justify-between gap-4">
					<div class="flex items-center gap-4">
						<!-- svelte-ignore a11y_consider_explicit_label -->
						<button
							onclick={() => {
								selectedLessonGroup = lesson_groups[lesson_header]!;
								dialog.showModal();
							}}
							class="flex-initial {lesson_groups[lesson_header]![0].colour} badge badge-xl"
						></button>
						{#await getFullModInfo(lesson_groups[lesson_header]![0].moduleCode, acadYear) then mod_info}
							<div class="flex flex-col">
								<div class="font-bold">{mod_info.moduleCode}</div>
								<div>{mod_info.title}</div>
							</div>
						{/await}
					</div>

					<X
						size={32}
						onclick={() => {
							currentlySelectedMods.set(
								removeModEntry(
									$currentlySelectedMods,
									timetable.academicYear,
									timetable.semester,
									timetable.id,
									timetable.name,
									lesson_groups[lesson_header]![0].moduleCode
								)
							);
						}}
					></X>
				</div>
			</div>
		</summary>
		<div class="collapse-content text-sm">
			{#each lesson_groups[lesson_header] as lesson_metadata}
				<div>
					{lesson_metadata.lessonType}: {lesson_metadata.lessonNo}
				</div>
			{/each}
		</div>
	</details>
</div>

<GenericDialog bind:dialog>
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

				dialog.close();
			}}
			class="flex-initial {colour} badge badge-lg"
		></button>
	{/each}
</GenericDialog>
