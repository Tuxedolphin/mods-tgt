<script lang="ts">
	import { currentlySelectedMods, preferences, searchTerm } from '$lib/shared/shared.svelte';
	import type { ModSummary } from '$lib/types/mod_summaries';
	import { getFullModInfo } from '$lib/utils/fetch_from_cache';
	import { checkModAlreadyAdded, createModEntry } from '$lib/utils/format_db_information';

	interface ModSuggestionsProp {
		mod: ModSummary;
		semester: number;
		acadYear: string;
		timetable_id: string;
		timetable_name: string;
	}

	let { mod, semester, acadYear, timetable_id, timetable_name }: ModSuggestionsProp = $props();

	async function addMod() {
		if (!mod.semesters.includes(semester)) return;
		const modFullInfo = await getFullModInfo(mod.moduleCode, acadYear);

		const timeTable = modFullInfo.semesterData.find((sem) => sem.semester == semester)!.timetable;

		if (
			checkModAlreadyAdded(
				$currentlySelectedMods,
				acadYear,
				semester,
				timetable_id,
				timetable_name,
				mod.moduleCode
			)
		)
			return;
		currentlySelectedMods.set(
			await createModEntry(
				$currentlySelectedMods,
				acadYear,
				semester,
				timetable_id,
				timetable_name,
				modFullInfo.moduleCode,
				timeTable
			)
		);

		searchTerm.set('');
	}
</script>

<button class="h-12 w-full" onclick={addMod}>
	{#if !mod.semesters.includes($preferences.currentSemView)}
		{mod.moduleCode} - {mod.title} - Not Offered in this semester
	{:else}
		{mod.moduleCode} - {mod.title}
	{/if}
</button>
