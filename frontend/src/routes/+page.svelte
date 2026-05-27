<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { currentUserInformation } from '../shared/shared.svelte';
	import { PUBLIC_DB_LINK } from '$env/static/public';
	import { login_to_db } from '../utils/db_operations';

	let nameInput = $state('');
	let emailInput = $state('');
	let passwordInput = $state('');
	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});

	async function login(username: string, password: string) {
		await login_to_db(username, password);
	}
</script>

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content text-center">
		<div class="max-w-md">
			<h1 class="text-5xl font-bold">Mods Together!</h1>
			<p class="py-4">We ride together. We mod together.</p>
			<div class="divider"></div>

			<div>Login Here!</div>

			<form class="fieldset content-around px-4">
				<fieldset class="fieldset">
					<label class="label">Email</label>
					<input
						type="email"
						class="validator input w-full"
						placeholder="Email"
						required
						bind:value={emailInput}
					/>
					<p class="$$validator-hint hidden">Required</p>
				</fieldset>

				<label class="fieldset">
					<span class="label">Password</span>
					<input
						type="password"
						class="validator input w-full"
						placeholder="Password"
						required
						bind:value={passwordInput}
					/>
					<span class="validator-hint hidden">Required</span>
				</label>

				<button
					class="btn mt-4 btn-neutral"
					type="submit"
					onclick={() => login(emailInput, passwordInput)}>Login</button
				>
			</form>

			<div class="divider">OR</div>

			<div>Try it out as a guest:</div>
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
		</div>
	</div>
</div>
