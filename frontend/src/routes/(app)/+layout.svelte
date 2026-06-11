<script lang="ts">
	import NavigationBar from './NavigationBar.svelte';

	import type { LayoutProps } from './$types';

	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	let { data, children, params }: LayoutProps = $props();
</script>

<div class="drawer">
	<input id="main-drawer" type="checkbox" class="drawer-toggle" />
	<div class="drawer-content">
		<NavigationBar {children} {data} {params}></NavigationBar>
	</div>

	<div class="drawer-side">
		<label for="main-drawer" aria-label="close sidebar" class="drawer-overlay"></label>
		<ul class="menu min-h-full w-64 bg-base-200 p-4">
			<!-- Sidebar content here -->
			<li><a>Sidebar Item 1</a></li>
			<li><a>Sidebar Item 2</a></li>
		</ul>
	</div>
</div>
