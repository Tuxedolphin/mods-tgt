<script lang="ts">
	import LoginButton from './LoginButton.svelte';

	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { registered, token_information } from '$lib/shared/shared.svelte';

	let emailInput = $state('');
	let passwordInput = $state('');
	let errorMessage = $state('');
	onMount(() => {
		for (const [key, value] of new URLSearchParams(window.location.hash)) {
			if (key.includes('error_description')) {
				errorMessage = value;
			}

			if (key.includes('access_token')) {
				$token_information.access_token = value;
				goto(resolve('/home'));
			}
		}
		registered.set(false);

		if ($token_information.access_token !== '' || $token_information.is_guest_login) {
			goto(resolve('/home'));
		}
	});
</script>

<div>Login Here!</div>
<div class="text-error">{errorMessage}</div>
<form class="fieldset content-around px-4">
	<fieldset class="fieldset">
		<!-- svelte-ignore a11y_label_has_associated_control -->
		<label class="label">Email</label>
		<input
			type="email"
			class="validator input w-full"
			placeholder="Email"
			required
			bind:value={emailInput}
		/>
		<p class="validator-hint hidden">Required</p>
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

	<LoginButton email={emailInput} password={passwordInput}></LoginButton>
</form>
<div>
	Don't have an account yet? Register <button
		class="cursor-pointer underline"
		onclick={() => goto(resolve('/register'))}>here</button
	>
</div>
