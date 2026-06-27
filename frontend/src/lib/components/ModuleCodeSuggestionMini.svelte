<script lang="ts">
	import { currentlySelectedMods, searchTerm } from '$lib/shared/shared.svelte';
	import type { ModSummary } from '$lib/types/mod_summaries';
	import { getFullModInfo } from '$lib/utils/fetch_from_cache';
	import { checkModAlreadyAdded, createModEntry } from '$lib/utils/format_db_information';

	interface ModSuggestionsProp {
		mod: ModSummary;
		semester: number;
		acadYear: string;
		timetable_id: string;
	}

	let { mod, semester, acadYear, timetable_id }: ModSuggestionsProp = $props();

	async function addMod() {
		if (!mod.semesters.includes(semester)) return;
		const modFullInfo = await getFullModInfo(mod.moduleCode, acadYear);

		const timeTable = modFullInfo.semesterData.find((sem) => sem.semester == semester)!.timetable;

		if (
			checkModAlreadyAdded($currentlySelectedMods, acadYear, semester, timetable_id, mod.moduleCode)
		)
			return;
		currentlySelectedMods.set(
			await createModEntry(
				$currentlySelectedMods,
				acadYear,
				semester,
				timetable_id,
				modFullInfo.moduleCode,
				timeTable
			)
		);

		searchTerm.set('');
	}
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="flex h-12 w-full items-center gap-2 p-4" onclick={addMod}>
	{#if !mod.semesters.includes(semester)}
		<p class="font-semibold">{mod.moduleCode}</p>
		<p>{mod.title}</p>
		<p class="opacity-65">Not Offered in this semester</p>
	{:else}
		<p class="font-semibold">{mod.moduleCode}</p>
		<p>{mod.title}</p>
	{/if}
</div>
