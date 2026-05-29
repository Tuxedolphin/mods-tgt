<script lang="ts">
	import GuestTryoutComponent from '../components/LoginPage/GuestTryoutComponent.svelte';

	import LoginComponent from '../components/LoginPage/LoginComponent.svelte';

	import ModTogetherHero from '../components/LoginPage/ModTogetherHero.svelte';

	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { currentUserInformation } from '../shared/shared.svelte';
	import { PUBLIC_DB_LINK } from '$env/static/public';

	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});
</script>

<p>{PUBLIC_DB_LINK}</p>
<ModTogetherHero>
	<LoginComponent></LoginComponent>
	<div class="divider">OR</div>

	<GuestTryoutComponent></GuestTryoutComponent>
</ModTogetherHero>
