<script lang="ts">
	import DaysOfWeekHeader from '$lib/components/DaysOfWeekHeader.svelte';
	import SearchBar from '$lib/components/SearchBar.svelte';
	import Timeline from '$lib/components/Timeline.svelte';
	import TimetableComponent from '$lib/components/TimetableComponent.svelte';
	import {
		currentlySelectedMods,
		currentUserInformation,
		currentWorkingTimetable,
		token_information
	} from '$lib/shared/shared.svelte';
	import { getTimetable } from '$lib/utils/format_db_information';
	import { onDestroy, onMount } from 'svelte';
	import type { PageProps } from './$types';
	import type {
		Profile,
		RoomInformation,
		TimetableDetailedResponse,
		TimetablePostTemplate,
		TimetableResponse
	} from '$lib/types/db_raw_types';
	import type { Unsubscriber } from 'svelte/store';
	import { format_AY_name, format_semester_name } from '$lib/utils/formatting_utils';
	import ModListGroup from '$lib/components/ModListGroup.svelte';
	import { CircleX } from '@lucide/svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { roomHub } from '$lib/stores/roomHub';
	import GenericDialog from '../../GenericDialog.svelte';

	let is_timetable_loaded = $state(false);
	let profiles: Profile[] = $state([]);
	let timetable_metadata: TimetableResponse = $state({
		academicYear: '',
		createdAt: '',
		id: '',
		metaData: [],
		name: '',
		semester: 0
	});
	// svelte-ignore non_reactive_update
	let share_tt_dialog: HTMLDialogElement;
	let copy_text = $state('Copy Link!');
	let currentTimetableDisplay = $derived(
		getTimetable(
			timetable_metadata.academicYear,
			timetable_metadata.semester,
			$currentlySelectedMods
		)
	);

	let { params }: PageProps = $props();
	let unsubscribe_from_mods_list: Unsubscriber;
	// svelte-ignore non_reactive_update
	let user_tt: TimetableDetailedResponse | undefined;
	onMount(async () => {
		// SignalR Related Actions
		await roomHub.connect($token_information.a);

		const info: RoomInformation | undefined = await $roomHub?.invoke(
			'CreateOrJoinRoom',
			params.timetable_id
		);
		is_timetable_loaded = false;

		timetable_metadata = info!.timetables[0];
		profiles = info!.users;
		$currentlySelectedMods = info!.timetables;

		// $roomHub?.on('ReceiveMessage', (msg) => console.log(msg));
		$roomHub?.on('ReceiveTimetableUpdate', (msg: TimetableDetailedResponse[]) => {
			update_from_room = true;
			$currentlySelectedMods = msg;
		});
		$roomHub?.on('ReceiveUserUpdate', (msg: Profile[]) => {
			profiles = msg;
		});

		// Find a timetable that belongs to current user:
		user_tt = info!.timetables.find((x) => x.profile.userId === $currentUserInformation.userId);

		if (!user_tt) {
			const info_to_post: TimetablePostTemplate = {
				academicYear: timetable_metadata.academicYear,
				metaData: [],
				name: timetable_metadata.name,
				semester: timetable_metadata.semester
			};
			await $roomHub?.invoke('CreateTimetable', info_to_post);

			roomHub.disconnect();

			window.location.reload();
		}

		$currentWorkingTimetable = user_tt?.id as string;

		let first_time_subscribe = true;
		let update_from_room = false;
		unsubscribe_from_mods_list = currentlySelectedMods.subscribe(async (updated_timetable) => {
			if (first_time_subscribe) {
				first_time_subscribe = false;
				return;
			}

			if (update_from_room) {
				update_from_room = false;
				return;
			}

			for (const timetable of updated_timetable) {
				if (timetable.id === user_tt?.id) {
					await $roomHub?.invoke('UpdateTimetable', timetable.id, {
						Name: timetable.name,
						MetaData: timetable.metaData
					});
				}
			}
		});
		is_timetable_loaded = true;
	});

	onDestroy(async () => {
		if (unsubscribe_from_mods_list) {
			unsubscribe_from_mods_list();
		}
		// await $roomHub?.invoke('LeaveRoom', params.timetable_id);

		roomHub.disconnect();

		currentlySelectedMods.reset();
	});
</script>

{#if is_timetable_loaded}
	<div class="flex items-center justify-between gap-2">
		<div class="flex min-w-0 flex-col">
			<h1 class="min-w-0 truncate text-lg font-semibold">
				{timetable_metadata.name}
			</h1>
			<h2 class="min-w-0 truncate text-xs">
				{format_AY_name(timetable_metadata.academicYear)} - {format_semester_name(
					timetable_metadata.semester
				)}
			</h2>
		</div>
		<div class="flex h-8 items-center gap-1">
			<div class="flex gap-1">
				{#each profiles as profile (profile.userId)}
					{#if profile.userId !== $currentUserInformation.userId}
						<div class="avatar avatar-placeholder">
							<div class="w-8 rounded-full bg-neutral text-neutral-content">
								<span class="text-xs">{profile.username?.charAt(0)}</span>
							</div>
						</div>
					{/if}
				{/each}
			</div>
			<button class="btn btn-accent" onclick={() => share_tt_dialog.show()}>
				Share Timetable
			</button>
			<CircleX
				class="min-w-6"
				size={32}
				onclick={() => {
					goto(resolve('/(app)/home'));
				}}
			></CircleX>
		</div>
	</div>
	<div class="flex">
		<Timeline></Timeline>
		<div class="flex-1 flex-col">
			<DaysOfWeekHeader
				acadYear={timetable_metadata.academicYear}
				semester={timetable_metadata.semester}
			></DaysOfWeekHeader>
			<TimetableComponent
				timetables={currentTimetableDisplay}
				acadYear={timetable_metadata.academicYear}
				semester={timetable_metadata.semester}
			></TimetableComponent>
		</div>
	</div>

	<SearchBar
		timetable_id={user_tt?.id as string}
		acadYear={timetable_metadata.academicYear}
		semester={timetable_metadata.semester}
	></SearchBar>

	<ModListGroup acadYear={timetable_metadata.academicYear}></ModListGroup>
{/if}

<GenericDialog bind:dialog={share_tt_dialog} closeHandler={() => (copy_text = 'Copy Link!')}>
	<h3 class="pb-2 text-lg font-bold">Share this timetable!</h3>

	<button
		class="btn w-full btn-primary"
		onclick={async () => {
			copy_text = 'Link Copied!';
			await navigator.clipboard.writeText(window.location.href);
		}}>{copy_text}</button
	>
</GenericDialog>
