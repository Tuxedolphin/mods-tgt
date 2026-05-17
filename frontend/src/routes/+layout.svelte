<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

<div class="navbar bg-base-100 shadow-sm">
  <a class="btn btn-ghost text-xl">Mods Together!</a>
</div>

{@render children()}
