<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { Unsubscriber } from 'svelte/store';
	import { currentlySelectedMods, currentUserInformation } from '$lib/shared/shared.svelte';
	import type { TimetableDetailedResponse } from '$lib/types/db_raw_types';
  import AddFromOtherTimetablesButton from './Buttons/AddFromOtherTimetablesButton.svelte';
	import ModListModInfo from './ModListModInfo.svelte';

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
	{#if user_own_modlist.metaData.length !== 0}
		<div class="grid gap-4 p-1 lg:grid-cols-3">
			<ModListModInfo timetable={user_own_modlist} {acadYear}></ModListModInfo>
		</div>
		{:else}
		<div class="flex flex-col">
			<div class="text-center">You do not have any mods</div>
				<div class="flex items-center justify-center">
					<AddFromOtherTimetablesButton acad_year={acadYear} semester={user_own_modlist.semester} current_timetable_id={user_own_modlist.id}></AddFromOtherTimetablesButton>
					 <div class="divider divider-horizontal">OR</div>
					<button class="btn btn-accent">Import from NUS Mods (Beta)</button>
				</div>
		</div>
		
	{/if}
{/if}

{#each other_mod_list as tt (tt.id)}
	<p>{tt.profile.username}'s Mod List:</p>
	<div class="grid gap-4 p-1 lg:grid-cols-3">
		<ModListModInfo timetable={tt} {acadYear}></ModListModInfo>
	</div>
{/each}


