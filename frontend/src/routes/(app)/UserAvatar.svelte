<script lang="ts">
	import { currentUserInformation, token_information } from '$lib/shared/shared.svelte';
	import GenericDialog from './GenericDialog.svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';

	let dialog: HTMLDialogElement;
</script>

<div class="avatar avatar-placeholder" onclick={() => dialog.show()}>
	<div class="w-12 rounded-full bg-neutral text-neutral-content">
		<span class="text-3xl">{$currentUserInformation.username?.charAt(0)}</span>
	</div>
</div>

<GenericDialog bind:dialog>
	<h3 class="text-lg font-bold">Hi, {$currentUserInformation.username}</h3>

	<button
		class="btn btn-error"
		onclick={async () => {
			token_information.reset();
			currentUserInformation.reset();
			const message = 'Logout Successful';
			goto(resolve(`/login#error_description=${message}`));
		}}>Logout</button
	>
</GenericDialog>
