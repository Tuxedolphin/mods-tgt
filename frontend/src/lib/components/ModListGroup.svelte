<script lang="ts">
	import type { TimetableDetailedResponse } from '$lib/types/db_raw_types';
	import { onDestroy, onMount } from 'svelte';
	import ModListModInfo from './ModListModInfo.svelte';
	import { currentlySelectedMods, currentUserInformation } from '$lib/shared/shared.svelte';
	import type { Unsubscriber } from 'svelte/store';
	let current_selected_mods_unsubsriber: Unsubscriber;

	interface ModListGroupProps {
		acadYear: string;
	}

	let { acadYear }: ModListGroupProps = $props();
	let user_own_modlist: TimetableDetailedResponse | undefined = $state();
	let other_mod_list: TimetableDetailedResponse[] = $state([]);

	onMount(() => {
		current_selected_mods_unsubsriber = currentlySelectedMods.subscribe((new_mods) => {
			user_own_modlist = new_mods.find((x) => x.profile.userId === $currentUserInformation.userId);
			other_mod_list = new_mods.filter((x) => x.profile.userId !== $currentUserInformation.userId);
		});
	});

	onDestroy(() => {
		current_selected_mods_unsubsriber();
	});
</script>

<p>Your Mod List:</p>
{#if user_own_modlist}
	<div class="grid grid-cols-3 gap-4 p-1">
		<ModListModInfo timetable={user_own_modlist} {acadYear}></ModListModInfo>
	</div>
{:else}
	<p>You don't have any mods added. Wanna add one?</p>
{/if}

{#each other_mod_list as tt (tt.id)}
	<p>{tt.profile.username}'s Mod List:</p>
	<div class="grid grid-cols-3 gap-4 p-1">
		<ModListModInfo timetable={tt} {acadYear}></ModListModInfo>
	</div>
{/each}
