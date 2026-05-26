<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { currentUserInformation } from '../shared/shared.svelte';

	let nameInput = $state('');

	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});
</script>

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content text-center">
		<div class="max-w-md">
			<h1 class="text-5xl font-bold">Mods Together!</h1>
			<p class="py-4">We ride together. We mod together.</p>

			<div class="py-4">
				<input type="text" placeholder="Enter your name" class="input" bind:value={nameInput} />
			</div>

			<button
				class="btn {nameInput.length === 0 ? 'btn-disabled' : ''} btn-primary"
				onclick={() => {
					$currentUserInformation.displayName = nameInput;
					$currentUserInformation.isGuest = true;
					goto(resolve('/planner'));
				}}>Plan as a guest</button
			>
			<div class="divider">OR</div>
		</div>
	</div>
</div>
