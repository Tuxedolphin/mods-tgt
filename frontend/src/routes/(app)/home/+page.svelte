<script lang="ts">
	import { onMount } from 'svelte';
	import GreetingComponent from '$lib/components/GreetingComponent.svelte';
	import { access_token } from '$lib/shared/shared.svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';

	onMount(() => {
		if (!$access_token.access_token) {
			goto(resolve('/login'));
		}
	});
</script>

<GreetingComponent></GreetingComponent>
