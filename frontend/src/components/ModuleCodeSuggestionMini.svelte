<script lang="ts">
	import { currentlySelectedMods } from '../shared/shared.svelte';
	import type { ModSummary } from '../types/mod_summaries';

	interface Mod {
		mod: ModSummary;
	}

	let { mod }: Mod = $props();

	async function addMod() {
		fetch('https://api.nusmods.com/v2/2025-2026/modules/{}')
		currentlySelectedMods.set({
			selectedMods: {}
		});
	}
</script>

<button class="h-12 w-full" onclick={addMod}>{mod.moduleCode} - {mod.title}</button>
