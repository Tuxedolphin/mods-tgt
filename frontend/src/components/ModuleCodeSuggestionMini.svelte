<script lang="ts">
	import { currentlySelectedMods, preferences, searchTerm } from '../shared/shared.svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import { getFullModInfo } from '../utils/fetch_from_cache';
	import { checkModAlreadyAdded, createModEntry } from '../utils/format_db_information';

	interface ModSuggestionsProp {
		mod: ModSummary;
		semester: number;
		acadYear: string;
	}

	let { mod, semester, acadYear }: ModSuggestionsProp = $props();

	async function addMod() {
		if (!mod.semesters.includes(semester)) return;
		const modFullInfo = await getFullModInfo(mod.moduleCode, acadYear);

		const timeTable = modFullInfo.semesterData.find((sem) => sem.semester == semester)!.timetable;

		if (
			checkModAlreadyAdded(
				$currentlySelectedMods,
				acadYear,
				semester,
				'you',
				'test',
				mod.moduleCode
			)
		)
			return;
		$currentlySelectedMods = await createModEntry(
			$currentlySelectedMods,
			acadYear,
			semester,
			'you',
			'test',
			modFullInfo.moduleCode,
			timeTable
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
