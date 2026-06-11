<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { currentUserInformation, token_information } from '$lib/shared/shared.svelte';
	import { onMount } from 'svelte';

	let nameInput = $state('');
	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/home'));
		}
	});
</script>

<div>Try it out as a guest:</div>
<div class="py-4">
	<input type="text" placeholder="Enter your name" class="input" bind:value={nameInput} />
</div>

<button
	class="btn {nameInput.length === 0 ? 'btn-disabled' : ''} btn-primary"
	onclick={() => {
		$currentUserInformation.displayName = nameInput;
		$token_information.b = true;
		$token_information.a = '';
		goto(resolve('/home'));
	}}>Plan as a guest</button
>
